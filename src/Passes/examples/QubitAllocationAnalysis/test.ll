; ModuleID = 'analysis-example.ll'
source_filename = "qir/ConstSizeArray.ll"

%Qubit = type opaque
%Array = type opaque

define internal fastcc void @Microsoft__Quantum__Tutorial__TeleportAndReset__body() unnamed_addr {
entry:
  %qubit = inttoptr i64 0 to %Qubit*
  call void @__quantum__qis__x__body(%Qubit* %qubit)
  %qubit__1 = inttoptr i64 1 to %Qubit*
  call void @__quantum__qis__x__body(%Qubit* %qubit__1)
  %qubit__2 = inttoptr i64 2 to %Qubit*
  call void @__quantum__qis__x__body(%Qubit* %qubit__2)
  ret void
}

declare %Array* @__quantum__rt__qubit_allocate_array(i64) local_unnamed_addr

declare void @__quantum__rt__qubit_release_array(%Array*) local_unnamed_addr

declare void @__quantum__rt__array_update_alias_count(%Array*, i32) local_unnamed_addr

declare i8* @__quantum__rt__array_get_element_ptr_1d(%Array*, i64) local_unnamed_addr

declare void @__quantum__qis__x__body(%Qubit*) local_unnamed_addr

define void @Microsoft__Quantum__Tutorial__TeleportAndReset__Interop() local_unnamed_addr #0 {
entry:
  call fastcc void @Microsoft__Quantum__Tutorial__TeleportAndReset__body()
  ret void
}

define void @Microsoft__Quantum__Tutorial__TeleportAndReset() local_unnamed_addr #1 {
entry:
  call fastcc void @Microsoft__Quantum__Tutorial__TeleportAndReset__body()
  ret void
}

attributes #0 = { "InteropFriendly" }
attributes #1 = { "EntryPoint" }
