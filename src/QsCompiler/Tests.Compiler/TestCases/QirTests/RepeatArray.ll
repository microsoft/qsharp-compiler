
%Array = type opaque
%Result = type opaque
%Qubit = type opaque
%String = type opaque

define internal i64 @Microsoft__Quantum__Testing__QIR__Main__body() {
entry:
  %count = alloca i64, align 8
  %register = call %Array* @__quantum__rt__qubit_allocate_array(i64 5)
  call void @__quantum__rt__array_update_alias_count(%Array* %register, i32 1)
  store i64 0, i64* %count, align 4
  br label %repeat__1

repeat__1:                                        ; preds = %exit__2, %entry
  call void @__quantum__rt__array_update_alias_count(%Array* %register, i32 1)
  %results = call %Array* @__quantum__qis__multim__body(%Array* %register)
  call void @__quantum__rt__array_update_alias_count(%Array* %register, i32 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %results, i32 1)
  %0 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %results, i64 0)
  %1 = bitcast i8* %0 to %Result**
  %2 = load %Result*, %Result** %1, align 8
  %3 = call %Result* @__quantum__rt__result_get_zero()
  %4 = call i1 @__quantum__rt__result_equal(%Result* %2, %Result* %3)
  br i1 %4, label %then0__1, label %continue__1

then0__1:                                         ; preds = %repeat__1
  %5 = load i64, i64* %count, align 4
  %6 = add i64 %5, 1
  store i64 %6, i64* %count, align 4
  br label %continue__1

continue__1:                                      ; preds = %then0__1, %repeat__1
  br label %until__1

until__1:                                         ; preds = %continue__1
  %7 = load i64, i64* %count, align 4
  %8 = icmp slt i64 %7, 5
  br i1 %8, label %rend__1, label %fixup__1

fixup__1:                                         ; preds = %until__1
  call void @__quantum__rt__array_update_alias_count(%Array* %results, i32 -1)
  %9 = call i64 @__quantum__rt__array_get_size_1d(%Array* %results)
  %10 = sub i64 %9, 1
  br label %header__2

rend__1:                                          ; preds = %until__1
  call void @__quantum__rt__array_update_alias_count(%Array* %results, i32 -1)
  %11 = call i64 @__quantum__rt__array_get_size_1d(%Array* %results)
  %12 = sub i64 %11, 1
  br label %header__1

header__1:                                        ; preds = %exiting__1, %rend__1
  %13 = phi i64 [ 0, %rend__1 ], [ %18, %exiting__1 ]
  %14 = icmp sle i64 %13, %12
  br i1 %14, label %body__1, label %exit__1

body__1:                                          ; preds = %header__1
  %15 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %results, i64 %13)
  %16 = bitcast i8* %15 to %Result**
  %17 = load %Result*, %Result** %16, align 8
  call void @__quantum__rt__result_update_reference_count(%Result* %17, i32 -1)
  br label %exiting__1

exiting__1:                                       ; preds = %body__1
  %18 = add i64 %13, 1
  br label %header__1

exit__1:                                          ; preds = %header__1
  call void @__quantum__rt__array_update_reference_count(%Array* %results, i32 -1)
  %19 = load i64, i64* %count, align 4
  call void @__quantum__rt__array_update_alias_count(%Array* %register, i32 -1)
  call void @__quantum__rt__qubit_release_array(%Array* %register)
  ret i64 %19

header__2:                                        ; preds = %exiting__2, %fixup__1
  %20 = phi i64 [ 0, %fixup__1 ], [ %25, %exiting__2 ]
  %21 = icmp sle i64 %20, %10
  br i1 %21, label %body__2, label %exit__2

body__2:                                          ; preds = %header__2
  %22 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %results, i64 %20)
  %23 = bitcast i8* %22 to %Result**
  %24 = load %Result*, %Result** %23, align 8
  call void @__quantum__rt__result_update_reference_count(%Result* %24, i32 -1)
  br label %exiting__2

exiting__2:                                       ; preds = %body__2
  %25 = add i64 %20, 1
  br label %header__2

exit__2:                                          ; preds = %header__2
  call void @__quantum__rt__array_update_reference_count(%Array* %results, i32 -1)
  br label %repeat__1
}

declare %Qubit* @__quantum__rt__qubit_allocate()

declare %Array* @__quantum__rt__qubit_allocate_array(i64)

declare void @__quantum__rt__qubit_release_array(%Array*)

declare void @__quantum__rt__array_update_alias_count(%Array*, i32)

declare %Array* @__quantum__qis__multim__body(%Array*)

declare i8* @__quantum__rt__array_get_element_ptr_1d(%Array*, i64)

declare %Result* @__quantum__rt__result_get_zero()

declare i1 @__quantum__rt__result_equal(%Result*, %Result*)

declare i64 @__quantum__rt__array_get_size_1d(%Array*)

declare void @__quantum__rt__result_update_reference_count(%Result*, i32)

declare void @__quantum__rt__array_update_reference_count(%Array*, i32)

define internal %Array* @Microsoft__Quantum__Testing__QIR__MultiM__body(%Array* %targets) {
entry:
  call void @__quantum__rt__array_update_alias_count(%Array* %targets, i32 1)
  %0 = call %Array* @__quantum__qis__multim__body(%Array* %targets)
  call void @__quantum__rt__array_update_alias_count(%Array* %targets, i32 -1)
  ret %Array* %0
}

define i64 @Microsoft__Quantum__Testing__QIR__Main__Interop() #0 {
entry:
  %0 = call i64 @Microsoft__Quantum__Testing__QIR__Main__body()
  ret i64 %0
}

define void @Microsoft__Quantum__Testing__QIR__Main() #1 {
entry:
  %0 = call i64 @Microsoft__Quantum__Testing__QIR__Main__body()
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
