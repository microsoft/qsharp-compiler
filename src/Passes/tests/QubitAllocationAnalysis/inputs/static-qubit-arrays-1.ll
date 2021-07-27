; ModuleID = 'qir/ConstSizeArray.ll'
source_filename = "qir/ConstSizeArray.ll"

%Array = type opaque
%String = type opaque

define internal fastcc void @Example__Main__body() unnamed_addr {
entry:
  call fastcc void @Example__QuantumProgram__body(i64 3)
  call fastcc void @Example__QuantumProgram__body(i64 4)
  ret void
}

define internal fastcc void @Example__QuantumProgram__body(i64 %x) unnamed_addr {
entry:
  %qubits = call %Array* @__quantum__rt__qubit_allocate_array(i64 %x)
  call void @__quantum__rt__array_update_alias_count(%Array* %qubits, i32 1)
  call void @__quantum__rt__array_update_alias_count(%Array* %qubits, i32 -1)
  call void @__quantum__rt__qubit_release_array(%Array* %qubits)
  ret void
}

declare %Array* @__quantum__rt__qubit_allocate_array(i64) local_unnamed_addr

declare void @__quantum__rt__qubit_release_array(%Array*) local_unnamed_addr

declare void @__quantum__rt__array_update_alias_count(%Array*, i32) local_unnamed_addr

declare void @__quantum__rt__string_update_reference_count(%String*, i32) local_unnamed_addr

define i64 @Example__Main__Interop() local_unnamed_addr #0 {
entry:
  call fastcc void @Example__Main__body()
  ret i64 0
}

define void @Example__Main() local_unnamed_addr #1 {
entry:
  call fastcc void @Example__Main__body()
  %0 = call %String* @__quantum__rt__int_to_string(i64 0)
  call void @__quantum__rt__message(%String* %0)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  ret void
}

declare void @__quantum__rt__message(%String*) local_unnamed_addr

declare %String* @__quantum__rt__int_to_string(i64) local_unnamed_addr

attributes #0 = { "InteropFriendly" }
attributes #1 = { "EntryPoint" }
