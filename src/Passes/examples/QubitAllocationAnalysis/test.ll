; ModuleID = 'analysis-example.ll'
source_filename = "qir/ConstSizeArray.ll"

%Qubit = type opaque
%Result = type opaque
%Tuple = type opaque
%Array = type opaque
%String = type opaque

@0 = internal constant [2 x i8] c"(\00"
@1 = internal constant [3 x i8] c", \00"
@2 = internal constant [2 x i8] c")\00"

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

define internal fastcc { %Result*, %Result* }* @TeleportChain__DemonstrateTeleportationUsingPresharedEntanglement__body() unnamed_addr {
entry:
  %leftMessage = call %Qubit* @__quantum__rt__qubit_allocate()
  %rightMessage = call %Qubit* @__quantum__rt__qubit_allocate()
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
  %9 = call %Tuple* @__quantum__rt__tuple_create(i64 16)
  %10 = bitcast %Tuple* %9 to { %Result*, %Result* }*
  %11 = bitcast %Tuple* %9 to %Result**
  %12 = getelementptr inbounds { %Result*, %Result* }, { %Result*, %Result* }* %10, i64 0, i32 1
  %13 = call fastcc %Result* @Microsoft__Quantum__Measurement__MResetZ__body(%Qubit* %leftMessage)
  %14 = inttoptr i64 3 to %Qubit*
  %15 = call fastcc %Result* @Microsoft__Quantum__Measurement__MResetZ__body(%Qubit* %14)
  store %Result* %13, %Result** %11, align 8
  store %Result* %15, %Result** %12, align 8
  call void @__quantum__rt__qubit_release(%Qubit* %leftMessage)
  call void @__quantum__rt__qubit_release(%Qubit* %rightMessage)
  ret { %Result*, %Result* }* %10
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

declare %Tuple* @__quantum__rt__tuple_create(i64) local_unnamed_addr

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

declare void @__quantum__rt__tuple_update_reference_count(%Tuple*, i32) local_unnamed_addr

declare void @__quantum__qis__cnot(%Qubit*, %Qubit*) local_unnamed_addr

declare void @__quantum__qis__h(%Qubit*) local_unnamed_addr

declare void @__quantum__qis__x(%Qubit*) local_unnamed_addr

declare void @__quantum__qis__z(%Qubit*) local_unnamed_addr

declare %String* @__quantum__rt__string_create(i8*) local_unnamed_addr

declare %Result* @__quantum__qis__m__body(%Qubit*) local_unnamed_addr

declare void @__quantum__qis__reset__body(%Qubit*) local_unnamed_addr

define { i8, i8 }* @TeleportChain__DemonstrateTeleportationUsingPresharedEntanglement__Interop() local_unnamed_addr #0 {
entry:
  %0 = call fastcc { %Result*, %Result* }* @TeleportChain__DemonstrateTeleportationUsingPresharedEntanglement__body()
  %1 = getelementptr inbounds { %Result*, %Result* }, { %Result*, %Result* }* %0, i64 0, i32 0
  %2 = getelementptr inbounds { %Result*, %Result* }, { %Result*, %Result* }* %0, i64 0, i32 1
  %3 = load %Result*, %Result** %1, align 8
  %4 = load %Result*, %Result** %2, align 8
  %5 = call %Result* @__quantum__rt__result_get_zero()
  %6 = call i1 @__quantum__rt__result_equal(%Result* %3, %Result* %5)
  %not. = xor i1 %6, true
  %7 = sext i1 %not. to i8
  %8 = call %Result* @__quantum__rt__result_get_zero()
  %9 = call i1 @__quantum__rt__result_equal(%Result* %4, %Result* %8)
  %not.1 = xor i1 %9, true
  %10 = sext i1 %not.1 to i8
  %11 = call i8* @__quantum__rt__memory_allocate(i64 2)
  %12 = bitcast i8* %11 to { i8, i8 }*
  store i8 %7, i8* %11, align 1
  %13 = getelementptr i8, i8* %11, i64 1
  store i8 %10, i8* %13, align 1
  call void @__quantum__rt__result_update_reference_count(%Result* %3, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %4, i32 -1)
  %14 = bitcast { %Result*, %Result* }* %0 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %14, i32 -1)
  ret { i8, i8 }* %12
}

declare %Result* @__quantum__rt__result_get_zero() local_unnamed_addr

declare i8* @__quantum__rt__memory_allocate(i64) local_unnamed_addr

define void @TeleportChain__DemonstrateTeleportationUsingPresharedEntanglement() local_unnamed_addr #1 {
entry:
  %0 = call fastcc { %Result*, %Result* }* @TeleportChain__DemonstrateTeleportationUsingPresharedEntanglement__body()
  %1 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @0, i64 0, i64 0))
  %2 = getelementptr inbounds { %Result*, %Result* }, { %Result*, %Result* }* %0, i64 0, i32 0
  %3 = getelementptr inbounds { %Result*, %Result* }, { %Result*, %Result* }* %0, i64 0, i32 1
  %4 = load %Result*, %Result** %2, align 8
  %5 = load %Result*, %Result** %3, align 8
  %6 = call %String* @__quantum__rt__result_to_string(%Result* %4)
  %7 = call %String* @__quantum__rt__string_concatenate(%String* %1, %String* %6)
  %8 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([3 x i8], [3 x i8]* @1, i64 0, i64 0))
  %9 = call %String* @__quantum__rt__string_concatenate(%String* %7, %String* %8)
  %10 = call %String* @__quantum__rt__result_to_string(%Result* %5)
  %11 = call %String* @__quantum__rt__string_concatenate(%String* %9, %String* %10)
  %12 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([2 x i8], [2 x i8]* @2, i64 0, i64 0))
  %13 = call %String* @__quantum__rt__string_concatenate(%String* %11, %String* %12)
  call void @__quantum__rt__message(%String* %13)
  call void @__quantum__rt__result_update_reference_count(%Result* %4, i32 -1)
  call void @__quantum__rt__result_update_reference_count(%Result* %5, i32 -1)
  %14 = bitcast { %Result*, %Result* }* %0 to %Tuple*
  call void @__quantum__rt__tuple_update_reference_count(%Tuple* %14, i32 -1)
  ret void
}

declare void @__quantum__rt__message(%String*) local_unnamed_addr

declare %String* @__quantum__rt__result_to_string(%Result*) local_unnamed_addr

declare void @__quantum__rt__string_update_reference_count(%String*, i32) local_unnamed_addr

declare %String* @__quantum__rt__string_concatenate(%String*, %String*) local_unnamed_addr

attributes #0 = { "InteropFriendly" }
attributes #1 = { "EntryPoint" }
