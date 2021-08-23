; ModuleID = 'qir/TeleportChain.ll'
source_filename = "qir/TeleportChain.ll"

%Qubit = type opaque
%Result = type opaque
%Array = type opaque
%String = type opaque

define internal fastcc void @TeleportChain__ApplyCorrection__body(%Qubit* %src, %Qubit* %intermediary, %Qubit* %dest) unnamed_addr {
entry:
  %0 = call fastcc %Result* @Microsoft__Quantum__Measurement__MResetZ__body(%Qubit* %src)
  %1 = call %Result* @__quantum__rt__result_get_one()
  %2 = call i1 @__quantum__rt__result_equal(%Result* %0, %Result* %1)
  call void @__quantum__rt__result_update_reference_count(%Result* %0, i32 -1)
  br i1 %2, label %then0__1, label %continue__1

then0__1:                                         ; preds = %entry
  call fastcc void @Microsoft__Quantum__Intrinsic__Z__body(%Qubit* %dest)
  br label %continue__1

continue__1:                                      ; preds = %then0__1, %entry
  %3 = call fastcc %Result* @Microsoft__Quantum__Measurement__MResetZ__body(%Qubit* %intermediary)
  %4 = call %Result* @__quantum__rt__result_get_one()
  %5 = call i1 @__quantum__rt__result_equal(%Result* %3, %Result* %4)
  call void @__quantum__rt__result_update_reference_count(%Result* %3, i32 -1)
  br i1 %5, label %then0__2, label %continue__2

then0__2:                                         ; preds = %continue__1
  call fastcc void @Microsoft__Quantum__Intrinsic__X__body(%Qubit* %dest)
  br label %continue__2

continue__2:                                      ; preds = %then0__2, %continue__1
  ret void
}

define internal fastcc %Result* @Microsoft__Quantum__Measurement__MResetZ__body(%Qubit* %target) unnamed_addr {
entry:
  %result = call %Result* @__quantum__qis__m__body(%Qubit* %target)
  call void @__quantum__qis__reset__body(%Qubit* %target)
  ret %Result* %result
}

declare %Result* @__quantum__rt__result_get_one() local_unnamed_addr

declare i1 @__quantum__rt__result_equal(%Result*, %Result*) local_unnamed_addr

declare void @__quantum__rt__result_update_reference_count(%Result*, i32) local_unnamed_addr

define internal fastcc void @Microsoft__Quantum__Intrinsic__Z__body(%Qubit* %qubit) unnamed_addr {
entry:
  call void @__quantum__qis__z(%Qubit* %qubit)
  ret void
}

define internal fastcc void @Microsoft__Quantum__Intrinsic__X__body(%Qubit* %qubit) unnamed_addr {
entry:
  call void @__quantum__qis__x(%Qubit* %qubit)
  ret void
}

define internal fastcc %Result* @TeleportChain__DemonstrateTeleportationUsingPresharedEntanglement__body() unnamed_addr {
entry:
  %t = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 3)
  %0 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %t, i64 0)
  %1 = bitcast i8* %0 to i64*
  %2 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %t, i64 1)
  %3 = bitcast i8* %2 to i64*
  %4 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %t, i64 2)
  %5 = bitcast i8* %4 to i64*
  store i64 1, i64* %1, align 4
  store i64 2, i64* %3, align 4
  store i64 3, i64* %5, align 4
  call void @__quantum__rt__array_update_alias_count(%Array* %t, i32 1)
  %leftMessage = call %Qubit* @__quantum__rt__qubit_allocate()
  %rightMessage = call %Qubit* @__quantum__rt__qubit_allocate()
  %leftPreshared = call %Array* @__quantum__rt__qubit_allocate_array(i64 2)
  call void @__quantum__rt__array_update_alias_count(%Array* %leftPreshared, i32 1)
  %rightPreshared = call %Array* @__quantum__rt__qubit_allocate_array(i64 2)
  call void @__quantum__rt__array_update_alias_count(%Array* %rightPreshared, i32 1)
  call fastcc void @TeleportChain__PrepareEntangledPair__body(%Qubit* %leftMessage, %Qubit* %rightMessage)
  %6 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %leftPreshared, i64 0)
  %7 = bitcast i8* %6 to %Qubit**
  %8 = load %Qubit*, %Qubit** %7, align 8
  %9 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %rightPreshared, i64 0)
  %10 = bitcast i8* %9 to %Qubit**
  %11 = load %Qubit*, %Qubit** %10, align 8
  call fastcc void @TeleportChain__PrepareEntangledPair__body(%Qubit* %8, %Qubit* %11)
  %12 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %leftPreshared, i64 1)
  %13 = bitcast i8* %12 to %Qubit**
  %14 = load %Qubit*, %Qubit** %13, align 8
  %15 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %rightPreshared, i64 1)
  %16 = bitcast i8* %15 to %Qubit**
  %17 = load %Qubit*, %Qubit** %16, align 8
  call fastcc void @TeleportChain__PrepareEntangledPair__body(%Qubit* %14, %Qubit* %17)
  %18 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %leftPreshared, i64 0)
  %19 = bitcast i8* %18 to %Qubit**
  %20 = load %Qubit*, %Qubit** %19, align 8
  %21 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %rightPreshared, i64 0)
  %22 = bitcast i8* %21 to %Qubit**
  %23 = load %Qubit*, %Qubit** %22, align 8
  call fastcc void @TeleportChain__TeleportQubitUsingPresharedEntanglement__body(%Qubit* %rightMessage, %Qubit* %20, %Qubit* %23)
  %24 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %rightPreshared, i64 0)
  %25 = bitcast i8* %24 to %Qubit**
  %26 = load %Qubit*, %Qubit** %25, align 8
  %27 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %leftPreshared, i64 1)
  %28 = bitcast i8* %27 to %Qubit**
  %29 = load %Qubit*, %Qubit** %28, align 8
  %30 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %rightPreshared, i64 1)
  %31 = bitcast i8* %30 to %Qubit**
  %32 = load %Qubit*, %Qubit** %31, align 8
  call fastcc void @TeleportChain__TeleportQubitUsingPresharedEntanglement__body(%Qubit* %26, %Qubit* %29, %Qubit* %32)
  %33 = call fastcc %Result* @Microsoft__Quantum__Measurement__MResetZ__body(%Qubit* %leftMessage)
  %34 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %rightPreshared, i64 1)
  %35 = bitcast i8* %34 to %Qubit**
  %36 = load %Qubit*, %Qubit** %35, align 8
  %37 = call fastcc %Result* @Microsoft__Quantum__Measurement__MResetZ__body(%Qubit* %36)
  call void @__quantum__rt__array_update_alias_count(%Array* %leftPreshared, i32 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %rightPreshared, i32 -1)
  call void @__quantum__rt__array_update_alias_count(%Array* %t, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %33, i32 -1)
  call void @__quantum__rt__array_update_reference_count(%Array* %t, i32 -1)
  call void @__quantum__rt__qubit_release(%Qubit* %leftMessage)
  call void @__quantum__rt__qubit_release(%Qubit* %rightMessage)
  call void @__quantum__rt__qubit_release_array(%Array* %leftPreshared)
  call void @__quantum__rt__qubit_release_array(%Array* %rightPreshared)
  ret %Result* %37
}

declare %Array* @__quantum__rt__array_create_1d(i32, i64) local_unnamed_addr

declare i8* @__quantum__rt__array_get_element_ptr_1d(%Array*, i64) local_unnamed_addr

declare void @__quantum__rt__array_update_alias_count(%Array*, i32) local_unnamed_addr

declare %Qubit* @__quantum__rt__qubit_allocate() local_unnamed_addr

declare %Array* @__quantum__rt__qubit_allocate_array(i64) local_unnamed_addr

declare void @__quantum__rt__qubit_release(%Qubit*) local_unnamed_addr

declare void @__quantum__rt__qubit_release_array(%Array*) local_unnamed_addr

define internal fastcc void @TeleportChain__PrepareEntangledPair__body(%Qubit* %left, %Qubit* %right) unnamed_addr {
entry:
  call fastcc void @Microsoft__Quantum__Intrinsic__H__body(%Qubit* %left)
  call fastcc void @Microsoft__Quantum__Intrinsic__CNOT__body(%Qubit* %left, %Qubit* %right)
  ret void
}

define internal fastcc void @TeleportChain__TeleportQubitUsingPresharedEntanglement__body(%Qubit* %src, %Qubit* %intermediary, %Qubit* %dest) unnamed_addr {
entry:
  call fastcc void @TeleportChain__PrepareEntangledPair__adj(%Qubit* %src, %Qubit* %intermediary)
  call fastcc void @TeleportChain__ApplyCorrection__body(%Qubit* %src, %Qubit* %intermediary, %Qubit* %dest)
  ret void
}

declare void @__quantum__rt__array_update_reference_count(%Array*, i32) local_unnamed_addr

define internal fastcc void @Microsoft__Quantum__Intrinsic__H__body(%Qubit* %qubit) unnamed_addr {
entry:
  call void @__quantum__qis__h(%Qubit* %qubit)
  ret void
}

define internal fastcc void @Microsoft__Quantum__Intrinsic__CNOT__body(%Qubit* %control, %Qubit* %target) unnamed_addr {
entry:
  call void @__quantum__qis__cnot(%Qubit* %control, %Qubit* %target)
  ret void
}

define internal fastcc void @TeleportChain__PrepareEntangledPair__adj(%Qubit* %left, %Qubit* %right) unnamed_addr {
entry:
  call fastcc void @Microsoft__Quantum__Intrinsic__CNOT__adj(%Qubit* %left, %Qubit* %right)
  call fastcc void @Microsoft__Quantum__Intrinsic__H__adj(%Qubit* %left)
  ret void
}

define internal fastcc void @Microsoft__Quantum__Intrinsic__CNOT__adj(%Qubit* %control, %Qubit* %target) unnamed_addr {
entry:
  call fastcc void @Microsoft__Quantum__Intrinsic__CNOT__body(%Qubit* %control, %Qubit* %target)
  ret void
}

define internal fastcc void @Microsoft__Quantum__Intrinsic__H__adj(%Qubit* %qubit) unnamed_addr {
entry:
  call fastcc void @Microsoft__Quantum__Intrinsic__H__body(%Qubit* %qubit)
  ret void
}

declare void @__quantum__qis__cnot(%Qubit*, %Qubit*) local_unnamed_addr

declare void @__quantum__qis__h(%Qubit*) local_unnamed_addr

declare void @__quantum__qis__x(%Qubit*) local_unnamed_addr

declare void @__quantum__qis__z(%Qubit*) local_unnamed_addr

declare %Result* @__quantum__qis__m__body(%Qubit*) local_unnamed_addr

declare void @__quantum__qis__reset__body(%Qubit*) local_unnamed_addr

define i8 @TeleportChain__DemonstrateTeleportationUsingPresharedEntanglement__Interop() local_unnamed_addr #0 {
entry:
  %0 = call fastcc %Result* @TeleportChain__DemonstrateTeleportationUsingPresharedEntanglement__body()
  %1 = call %Result* @__quantum__rt__result_get_zero()
  %2 = call i1 @__quantum__rt__result_equal(%Result* %0, %Result* %1)
  %not. = xor i1 %2, true
  %3 = sext i1 %not. to i8
  call void @__quantum__rt__result_update_reference_count(%Result* %0, i32 -1)
  ret i8 %3
}

declare %Result* @__quantum__rt__result_get_zero() local_unnamed_addr

define void @TeleportChain__DemonstrateTeleportationUsingPresharedEntanglement() local_unnamed_addr #1 {
entry:
  %0 = call fastcc %Result* @TeleportChain__DemonstrateTeleportationUsingPresharedEntanglement__body()
  %1 = call %String* @__quantum__rt__result_to_string(%Result* %0)
  call void @__quantum__rt__message(%String* %1)
  call void @__quantum__rt__result_update_reference_count(%Result* %0, i32 -1)
  call void @__quantum__rt__string_update_reference_count(%String* %1, i32 -1)
  ret void
}

declare void @__quantum__rt__message(%String*) local_unnamed_addr

declare %String* @__quantum__rt__result_to_string(%Result*) local_unnamed_addr

declare void @__quantum__rt__string_update_reference_count(%String*, i32) local_unnamed_addr

attributes #0 = { "InteropFriendly" }
attributes #1 = { "EntryPoint" }
