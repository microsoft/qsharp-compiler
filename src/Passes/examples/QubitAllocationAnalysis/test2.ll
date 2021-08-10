; ModuleID = 'analysis-example.ll'
source_filename = "qir/ConstSizeArray.ll"

%Qubit = type opaque
%Result = type opaque
%Array = type opaque
%String = type opaque

@0 = internal constant [3 x i8] c"()\00"

define internal fastcc void @TeleportChain__ApplyCorrection__body(%Qubit* %src, %Qubit* %intermediary, %Qubit* %dest) unnamed_addr {
entry:
  %0 = call fastcc %Result* @Microsoft__Quantum__Measurement__MResetZ__body(%Qubit* %src)
  %1 = call i1 @__quantum__qir__read_result(%Result* %0)
  br i1 %1, label %then0__1, label %continue__1

then0__1:                                         ; preds = %entry
  call fastcc void @Microsoft__Quantum__Intrinsic__Z__body(%Qubit* %dest)
  br label %continue__1

continue__1:                                      ; preds = %then0__1, %entry
  %2 = call fastcc %Result* @Microsoft__Quantum__Measurement__MResetZ__body(%Qubit* %intermediary)
  %3 = call i1 @__quantum__qir__read_result(%Result* %2)
  br i1 %3, label %then0__2, label %continue__2

then0__2:                                         ; preds = %continue__1
  call fastcc void @Microsoft__Quantum__Intrinsic__X__body(%Qubit* %dest)
  br label %continue__2

continue__2:                                      ; preds = %then0__2, %continue__1
  ret void
}

define internal fastcc %Result* @Microsoft__Quantum__Measurement__MResetZ__body(%Qubit* %target) unnamed_addr {
entry:
  %result = inttoptr i64 0 to %Result*
  call void @__quantum__qis__mz__body(%Qubit* %target, %Result* %result)
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

define internal fastcc void @TeleportChain__DemonstrateTeleportationUsingPresharedEntanglement__body() unnamed_addr {
entry:
  %leftMessage = inttoptr i64 0 to %Qubit*
  %rightMessage = inttoptr i64 1 to %Qubit*
  call fastcc void @TeleportChain__PrepareEntangledPair__body(%Qubit* %leftMessage, %Qubit* %rightMessage)
  %0 = inttoptr i64 0 to %Qubit*
  %1 = inttoptr i64 2 to %Qubit*
  call fastcc void @TeleportChain__PrepareEntangledPair__body(%Qubit* %0, %Qubit* %1)
  %2 = inttoptr i64 1 to %Qubit*
  %3 = inttoptr i64 3 to %Qubit*
  call fastcc void @TeleportChain__PrepareEntangledPair__body(%Qubit* %2, %Qubit* %3)
  %4 = inttoptr i64 0 to %Qubit*
  %5 = inttoptr i64 2 to %Qubit*
  call fastcc void @TeleportChain__TeleportQubitUsingPresharedEntanglement__body(%Qubit* %rightMessage, %Qubit* %4, %Qubit* %5)
  %6 = inttoptr i64 2 to %Qubit*
  %7 = inttoptr i64 1 to %Qubit*
  %8 = inttoptr i64 3 to %Qubit*
  call fastcc void @TeleportChain__TeleportQubitUsingPresharedEntanglement__body(%Qubit* %6, %Qubit* %7, %Qubit* %8)
  %9 = call fastcc %Result* @Microsoft__Quantum__Measurement__MResetZ__body(%Qubit* %leftMessage)
  %10 = inttoptr i64 3 to %Qubit*
  %11 = call fastcc %Result* @Microsoft__Quantum__Measurement__MResetZ__body(%Qubit* %10)
  ret void
}

declare %Qubit* @__quantum__rt__qubit_allocate() local_unnamed_addr

declare %Array* @__quantum__rt__qubit_allocate_array(i64) local_unnamed_addr

declare void @__quantum__rt__qubit_release(%Qubit*) local_unnamed_addr

declare void @__quantum__rt__qubit_release_array(%Array*) local_unnamed_addr

declare void @__quantum__rt__array_update_alias_count(%Array*, i32) local_unnamed_addr

define internal fastcc void @TeleportChain__PrepareEntangledPair__body(%Qubit* %left, %Qubit* %right) unnamed_addr {
entry:
  call fastcc void @Microsoft__Quantum__Intrinsic__H__body(%Qubit* %left)
  call fastcc void @Microsoft__Quantum__Intrinsic__CNOT__body(%Qubit* %left, %Qubit* %right)
  ret void
}

declare i8* @__quantum__rt__array_get_element_ptr_1d(%Array*, i64) local_unnamed_addr

define internal fastcc void @TeleportChain__TeleportQubitUsingPresharedEntanglement__body(%Qubit* %src, %Qubit* %intermediary, %Qubit* %dest) unnamed_addr {
entry:
  call fastcc void @TeleportChain__PrepareEntangledPair__adj(%Qubit* %src, %Qubit* %intermediary)
  call fastcc void @TeleportChain__ApplyCorrection__body(%Qubit* %src, %Qubit* %intermediary, %Qubit* %dest)
  ret void
}

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

declare %String* @__quantum__rt__string_create(i8*) local_unnamed_addr

declare %Result* @__quantum__qis__m__body(%Qubit*) local_unnamed_addr

declare void @__quantum__qis__reset__body(%Qubit*) local_unnamed_addr

define void @TeleportChain__DemonstrateTeleportationUsingPresharedEntanglement__Interop() local_unnamed_addr #0 {
entry:
  call fastcc void @TeleportChain__DemonstrateTeleportationUsingPresharedEntanglement__body()
  ret void
}

define void @TeleportChain__DemonstrateTeleportationUsingPresharedEntanglement() local_unnamed_addr #1 {
entry:
  call fastcc void @TeleportChain__DemonstrateTeleportationUsingPresharedEntanglement__body()
  ret void
}

declare void @__quantum__rt__message(%String*) local_unnamed_addr

declare void @__quantum__rt__string_update_reference_count(%String*, i32) local_unnamed_addr

declare i1 @__quantum__qir__read_result(%Result*)

declare void @__quantum__qis__mz__body(%Qubit*, %Result*)

attributes #0 = { "InteropFriendly" }
attributes #1 = { "EntryPoint" }
