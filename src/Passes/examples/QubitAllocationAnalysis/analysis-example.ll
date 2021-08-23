; ModuleID = 'qir/TeleportChain.ll'
source_filename = "qir/TeleportChain.ll"

%Qubit = type opaque
%String = type opaque

define internal fastcc void @TeleportChain__Main__body() unnamed_addr {
entry:
  %q = call fastcc %Qubit* @TeleportChain__RandomQubit__body()
  call fastcc void @Microsoft__Quantum__Intrinsic__H__body(%Qubit* %q)
  ret void
}

define internal fastcc %Qubit* @TeleportChain__RandomQubit__body() unnamed_addr {
entry:
  %q1 = call %Qubit* @__quantum__rt__qubit_allocate()
  call void @__quantum__rt__qubit_release(%Qubit* %q1)
  ret %Qubit* %q1
}

define internal fastcc void @Microsoft__Quantum__Intrinsic__H__body(%Qubit* %qubit) unnamed_addr {
entry:
  call void @__quantum__qis__h(%Qubit* %qubit)
  ret void
}

declare %Qubit* @__quantum__rt__qubit_allocate() local_unnamed_addr

declare void @__quantum__rt__qubit_release(%Qubit*) local_unnamed_addr

declare void @__quantum__qis__h(%Qubit*) local_unnamed_addr

define i64 @TeleportChain__Main__Interop() local_unnamed_addr #0 {
entry:
  call fastcc void @TeleportChain__Main__body()
  ret i64 0
}

define void @TeleportChain__Main() local_unnamed_addr #1 {
entry:
  call fastcc void @TeleportChain__Main__body()
  %0 = call %String* @__quantum__rt__int_to_string(i64 0)
  call void @__quantum__rt__message(%String* %0)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  ret void
}

declare void @__quantum__rt__message(%String*) local_unnamed_addr

declare %String* @__quantum__rt__int_to_string(i64) local_unnamed_addr

declare void @__quantum__rt__string_update_reference_count(%String*, i32) local_unnamed_addr

attributes #0 = { "InteropFriendly" }
attributes #1 = { "EntryPoint" }
