; ModuleID = 'qat-link'
source_filename = "qat-link"

%Result = type opaque
%Qubit = type opaque
%Array = type opaque
%String = type opaque

declare %Result* @__quantum__rt__result_get_zero() local_unnamed_addr

declare i1 @__quantum__rt__result_equal(%Result*, %Result*) local_unnamed_addr

declare void @__quantum__rt__result_update_reference_count(%Result*, i32) local_unnamed_addr

declare %Qubit* @__quantum__rt__qubit_allocate() local_unnamed_addr

declare %Array* @__quantum__rt__qubit_allocate_array(i64) local_unnamed_addr

declare void @__quantum__rt__array_update_alias_count(%Array*, i32) local_unnamed_addr

declare i8* @__quantum__rt__array_get_element_ptr_1d(%Array*, i64) local_unnamed_addr

declare void @__quantum__rt__qubit_release(%Qubit*) local_unnamed_addr

declare void @__quantum__rt__qubit_release_array(%Array*) local_unnamed_addr

declare %Result* @__quantum__qis__m__body(%Qubit*) local_unnamed_addr

declare void @__quantum__qis__reset__body(%Qubit*) local_unnamed_addr

declare %Result* @__quantum__rt__result_get_one() local_unnamed_addr

declare void @__quantum__qis__x__body(%Qubit*) local_unnamed_addr

declare void @__quantum__qis__z__body(%Qubit*) local_unnamed_addr

declare void @__quantum__qis__h__body(%Qubit*) local_unnamed_addr

declare void @__quantum__qis__cnot__body(%Qubit*, %Qubit*) local_unnamed_addr

define void @TeleportChain__DemonstrateTeleportationUsingPresharedEntanglement() local_unnamed_addr #0 {
entry:
  tail call void @__quantum__qis__h__body(%Qubit* null)
  tail call void @__quantum__qis__cnot__body(%Qubit* null, %Qubit* nonnull inttoptr (i64 1 to %Qubit*))
  tail call void @__quantum__qis__h__body(%Qubit* nonnull inttoptr (i64 2 to %Qubit*))
  tail call void @__quantum__qis__cnot__body(%Qubit* nonnull inttoptr (i64 2 to %Qubit*), %Qubit* nonnull inttoptr (i64 4 to %Qubit*))
  tail call void @__quantum__qis__h__body(%Qubit* nonnull inttoptr (i64 3 to %Qubit*))
  tail call void @__quantum__qis__cnot__body(%Qubit* nonnull inttoptr (i64 3 to %Qubit*), %Qubit* nonnull inttoptr (i64 5 to %Qubit*))
  tail call void @__quantum__qis__cnot__body(%Qubit* nonnull inttoptr (i64 1 to %Qubit*), %Qubit* nonnull inttoptr (i64 2 to %Qubit*))
  tail call void @__quantum__qis__h__body(%Qubit* nonnull inttoptr (i64 1 to %Qubit*))
  tail call void @__quantum__qis__mz__body(%Qubit* nonnull inttoptr (i64 1 to %Qubit*), %Result* null)
  tail call void @__quantum__qis__reset__body(%Qubit* nonnull inttoptr (i64 1 to %Qubit*))
  %0 = tail call i1 @__quantum__qir__read_result(%Result* null)
  br i1 %0, label %then0__1.i.i.i, label %continue__1.i.i.i

then0__1.i.i.i:                                   ; preds = %entry
  tail call void @__quantum__qis__z__body(%Qubit* nonnull inttoptr (i64 4 to %Qubit*))
  br label %continue__1.i.i.i

continue__1.i.i.i:                                ; preds = %then0__1.i.i.i, %entry
  tail call void @__quantum__qis__mz__body(%Qubit* nonnull inttoptr (i64 2 to %Qubit*), %Result* nonnull inttoptr (i64 1 to %Result*))
  tail call void @__quantum__qis__reset__body(%Qubit* nonnull inttoptr (i64 2 to %Qubit*))
  %1 = tail call i1 @__quantum__qir__read_result(%Result* nonnull inttoptr (i64 1 to %Result*))
  br i1 %1, label %then0__2.i.i.i, label %TeleportChain__TeleportQubitUsingPresharedEntanglement__body.2.exit.i

then0__2.i.i.i:                                   ; preds = %continue__1.i.i.i
  tail call void @__quantum__qis__x__body(%Qubit* nonnull inttoptr (i64 4 to %Qubit*))
  br label %TeleportChain__TeleportQubitUsingPresharedEntanglement__body.2.exit.i

TeleportChain__TeleportQubitUsingPresharedEntanglement__body.2.exit.i: ; preds = %then0__2.i.i.i, %continue__1.i.i.i
  tail call void @__quantum__qis__cnot__body(%Qubit* nonnull inttoptr (i64 4 to %Qubit*), %Qubit* nonnull inttoptr (i64 3 to %Qubit*))
  tail call void @__quantum__qis__h__body(%Qubit* nonnull inttoptr (i64 4 to %Qubit*))
  tail call void @__quantum__qis__mz__body(%Qubit* nonnull inttoptr (i64 4 to %Qubit*), %Result* nonnull inttoptr (i64 2 to %Result*))
  tail call void @__quantum__qis__reset__body(%Qubit* nonnull inttoptr (i64 4 to %Qubit*))
  %2 = tail call i1 @__quantum__qir__read_result(%Result* nonnull inttoptr (i64 2 to %Result*))
  br i1 %2, label %then0__1.i.i1.i, label %continue__1.i.i2.i

then0__1.i.i1.i:                                  ; preds = %TeleportChain__TeleportQubitUsingPresharedEntanglement__body.2.exit.i
  tail call void @__quantum__qis__z__body(%Qubit* nonnull inttoptr (i64 5 to %Qubit*))
  br label %continue__1.i.i2.i

continue__1.i.i2.i:                               ; preds = %then0__1.i.i1.i, %TeleportChain__TeleportQubitUsingPresharedEntanglement__body.2.exit.i
  tail call void @__quantum__qis__mz__body(%Qubit* nonnull inttoptr (i64 3 to %Qubit*), %Result* nonnull inttoptr (i64 3 to %Result*))
  tail call void @__quantum__qis__reset__body(%Qubit* nonnull inttoptr (i64 3 to %Qubit*))
  %3 = tail call i1 @__quantum__qir__read_result(%Result* nonnull inttoptr (i64 3 to %Result*))
  br i1 %3, label %then0__2.i.i3.i, label %TeleportChain__DemonstrateTeleportationUsingPresharedEntanglement__body.1.exit

then0__2.i.i3.i:                                  ; preds = %continue__1.i.i2.i
  tail call void @__quantum__qis__x__body(%Qubit* nonnull inttoptr (i64 5 to %Qubit*))
  br label %TeleportChain__DemonstrateTeleportationUsingPresharedEntanglement__body.1.exit

TeleportChain__DemonstrateTeleportationUsingPresharedEntanglement__body.1.exit: ; preds = %continue__1.i.i2.i, %then0__2.i.i3.i
  tail call void @__quantum__qis__mz__body(%Qubit* null, %Result* nonnull inttoptr (i64 4 to %Result*))
  tail call void @__quantum__qis__reset__body(%Qubit* null)
  tail call void @__quantum__qis__mz__body(%Qubit* nonnull inttoptr (i64 5 to %Qubit*), %Result* nonnull inttoptr (i64 5 to %Result*))
  tail call void @__quantum__qis__reset__body(%Qubit* nonnull inttoptr (i64 5 to %Qubit*))
  %4 = tail call %String* @__quantum__rt__result_to_string(%Result* nonnull inttoptr (i64 5 to %Result*))
  ret void
}

declare %String* @__quantum__rt__result_to_string(%Result*) local_unnamed_addr

declare void @__quantum__rt__message(%String*) local_unnamed_addr

declare void @__quantum__rt__string_update_reference_count(%String*, i32) local_unnamed_addr

declare void @__quantum__qis__mz__body(%Qubit*, %Result*)

declare i1 @__quantum__qir__read_result(%Result*)

attributes #0 = { "EntryPoint" "requiredQubits"="6" }

